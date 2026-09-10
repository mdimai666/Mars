// Нагрузочные сценарии Mars (k6 v2).
//
// Запуск одного сценария (рекомендуется — сценарии не мешают друг другу):
//   $env:MARS_URL="http://localhost:PORT"; $env:MARS_SCENARIO="render_static_anon"; k6 run benchmarks/k6/run-all.js
//
// Запуск всех сразу (параллельно):
//   k6 run benchmarks/k6/run-all.js
//
// Базлайнные сценарии (constant-vus, окно DURATION — по умолчанию 30s):
//   render_static_anon / render_static_auth — рендер страницы без БД ("/")
//   render_db_anon    / render_db_auth     — рендер страницы с запросом к БД ("/posts")
//   post_read   — GET поста по slug (API)
//   post_list   — постраничный список постов (API)
//   post_create — POST /api/Post (Bearer)
//   post_update — GET + PUT /api/Post (Bearer)
//
// Стресс-сценарии (ramping-vus 0 -> STRESS_MAX_VUS, ~2.5 мин) — поиск потолка сервера:
//   stress_render_static, stress_render_db, stress_post_create

import http from 'k6/http';
import { check } from 'k6';
import execution from 'k6/execution';
import {
    BASE_URL,
    LOGIN,
    PASSWORD,
    POSTS_COUNT,
    READ_VUS,
    WRITE_VUS,
    DURATION,
    STRESS_MAX_VUS,
    SMOKE_ITERATIONS,
} from './config.js';

const API = `${BASE_URL}/api`;

// Режим прогона: full — полные 30s-окна (default), smoke — короткие 1000 итераций.
const MODE = __ENV.MARS_MODE === 'smoke' ? 'smoke' : 'full';

// Базлайн: фиксированное число VU на фиксированное окно — стационарный режим,
// сопоставимые прогоны от версии к версии.
function constantVus(execFn, vus, duration) {
    return {
        executor: 'constant-vus',
        exec: execFn,
        vus: vus,
        duration: duration,
        gracefulStop: '30s',
    };
}

// Smoke: быстро (суммарно ~минута), ловит явные просадки.
function sharedIterations(execFn, vus, iterations) {
    return {
        executor: 'shared-iterations',
        exec: execFn,
        vus: vus,
        iterations: iterations,
        maxDuration: '10m',
        gracefulStop: '30s',
    };
}

function baseline(execFn, vus) {
    return MODE === 'smoke'
        ? sharedIterations(execFn, vus, SMOKE_ITERATIONS)
        : constantVus(execFn, vus, DURATION);
}

// Стресс: разгон VU до потолка и полка — смотрим, где RPS перестаёт расти,
// а p95 уходит вверх (точка насыщения сервера).
function stress(execFn, maxVus) {
    return {
        executor: 'ramping-vus',
        exec: execFn,
        stages: [
            { duration: '30s', target: Math.floor(maxVus / 2) },
            { duration: '1m', target: maxVus },
            { duration: '30s', target: maxVus },
            { duration: '15s', target: 0 },
        ],
        gracefulStop: '30s',
    };
}

const ALL_SCENARIOS = {
    render_static_anon: baseline('renderStaticAnon', READ_VUS),
    render_static_auth: baseline('renderStaticAuth', READ_VUS),
    render_db_anon: baseline('renderDbAnon', READ_VUS),
    render_db_auth: baseline('renderDbAuth', READ_VUS),
    post_read: baseline('postRead', READ_VUS),
    post_list: baseline('postList', READ_VUS),
    post_update: baseline('postUpdate', WRITE_VUS),
    post_create: baseline('postCreate', WRITE_VUS),

    stress_render_static: stress('renderStaticAnon', STRESS_MAX_VUS),
    stress_render_db: stress('renderDbAnon', STRESS_MAX_VUS),
    stress_post_create: stress('postCreate', Math.floor(STRESS_MAX_VUS / 2)),
};

// MARS_SCENARIO=render_db_anon — только один сценарий (k6 v2 не имеет флага --scenario);
// без него — все сценарии параллельно.
const ONLY_SCENARIO = __ENV.MARS_SCENARIO;
if (ONLY_SCENARIO && !ALL_SCENARIOS[ONLY_SCENARIO]) {
    throw new Error(`Неизвестный сценарий '${ONLY_SCENARIO}'. Доступны: ${Object.keys(ALL_SCENARIOS).join(', ')}`);
}

export const options = {
    scenarios: ONLY_SCENARIO ? { [ONLY_SCENARIO]: ALL_SCENARIOS[ONLY_SCENARIO] } : ALL_SCENARIOS,
    thresholds: {
        'http_req_failed': ['rate<0.01'],
        'checks': ['rate>0.99'],
    },
};

// Логин выполняется один раз до старта сценариев, токен раздаётся всем VU.
export function setup() {
    const res = http.post(
        `${API}/Account/Login`,
        JSON.stringify({ login: LOGIN, password: PASSWORD }),
        { headers: { 'Content-Type': 'application/json' }, tags: { name: 'Login' } }
    );
    if (res.status !== 200) {
        throw new Error(`Login failed: status=${res.status} body=${res.body}`);
    }
    const token = res.json('token');
    if (!token) {
        throw new Error(`Login response has no token: ${res.body}`);
    }
    return { token: token };
}

function authHeaders(data) {
    return { 'Authorization': `Bearer ${data.token}` };
}

function postSlug(n) {
    return `post-${String(n).padStart(4, '0')}`;
}

function randomPostNumber() {
    return 1 + Math.floor(Math.random() * POSTS_COUNT);
}

// ==========================================
// Рендер страниц (front: Handlebars шаблон)
// ==========================================

export function renderStaticAnon() {
    const res = http.get(`${BASE_URL}/`, { tags: { name: 'GET / (static)' } });
    check(res, {
        'static: status 200': (r) => r.status === 200,
        'static: marker': (r) => r.body.indexOf('Welcome to Mars Handlebars Site') !== -1,
    });
}

export function renderStaticAuth(data) {
    const res = http.get(`${BASE_URL}/`, {
        headers: authHeaders(data),
        tags: { name: 'GET / (static, auth)' },
    });
    check(res, {
        'static auth: status 200': (r) => r.status === 200,
        'static auth: marker': (r) => r.body.indexOf('Welcome to Mars Handlebars Site') !== -1,
    });
}

export function renderDbAnon() {
    const res = http.get(`${BASE_URL}/posts`, { tags: { name: 'GET /posts (db)' } });
    check(res, {
        'db: status 200': (r) => r.status === 200,
        'db: posts rendered': (r) => r.body.indexOf('Perf post') !== -1,
    });
}

export function renderDbAuth(data) {
    const res = http.get(`${BASE_URL}/posts`, {
        headers: authHeaders(data),
        tags: { name: 'GET /posts (db, auth)' },
    });
    check(res, {
        'db auth: status 200': (r) => r.status === 200,
        'db auth: posts rendered': (r) => r.body.indexOf('Perf post') !== -1,
    });
}

// ==========================================
// API постов: чтение
// ==========================================

export function postRead() {
    const slug = postSlug(randomPostNumber());
    const res = http.get(`${API}/Post/by-type/post/item/${slug}`, {
        tags: { name: 'GET /api/Post/by-type/{type}/item/{slug}' },
    });
    check(res, {
        'read: status 200': (r) => r.status === 200,
        'read: slug matches': (r) => r.json('slug') === slug,
    });
}

export function postList() {
    // 1000 постов по 20 на страницу = 50 страниц
    const page = 1 + Math.floor(Math.random() * Math.max(1, Math.ceil(POSTS_COUNT / 20)));
    const res = http.get(`${API}/Post/list/page?page=${page}&pageSize=20`, {
        tags: { name: 'GET /api/Post/list/page' },
    });
    check(res, {
        'list: status 200': (r) => r.status === 200,
        'list: has items': (r) => (r.json('items') || []).length > 0,
    });
}

// ==========================================
// API постов: запись
// ==========================================

export function postCreate(data) {
    const uniq = `${execution.vu.idInTest}-${execution.scenario.iterationInTest}-${Date.now()}`;
    const slug = `k6-c-${uniq}`;
    const res = http.post(
        `${API}/Post`,
        JSON.stringify({
            id: null,
            title: `k6 create ${uniq}`,
            type: 'post',
            slug: slug,
            tags: [],
            content: '<p>Создан нагрузочным сценарием k6.</p>',
            status: 'publish',
            excerpt: '',
            langCode: '',
            categoryIds: [],
            metaValues: [],
        }),
        {
            headers: Object.assign({ 'Content-Type': 'application/json' }, authHeaders(data)),
            tags: { name: 'POST /api/Post' },
        }
    );
    check(res, { 'create: status 201': (r) => r.status === 201 });
}

export function postUpdate(data) {
    // Каждый VU обновляет свой блок постов, чтобы не конфликтовать по Version-токену
    const blockSize = Math.max(1, Math.ceil(POSTS_COUNT / WRITE_VUS));
    const n = (execution.vu.idInTest - 1) * blockSize
        + (execution.scenario.iterationInTest % blockSize)
        + 1;
    const slug = postSlug(n);

    const readRes = http.get(`${API}/Post/by-type/post/item/${slug}`, {
        tags: { name: 'GET (before PUT)' },
    });
    if (!check(readRes, { 'update: read 200': (r) => r.status === 200 })) {
        return;
    }

    const doc = readRes.json();
    const res = http.put(
        `${API}/Post`,
        JSON.stringify({
            id: doc.id,
            title: `${doc.title.split(' | k6 u')[0]} | k6 u${execution.scenario.iterationInTest}`,
            type: 'post',
            slug: doc.slug,
            tags: doc.tags || [],
            content: doc.content,
            status: 'publish',
            excerpt: '',
            langCode: '',
            categoryIds: [],
            metaValues: [],
        }),
        {
            headers: Object.assign({ 'Content-Type': 'application/json' }, authHeaders(data)),
            tags: { name: 'PUT /api/Post' },
        }
    );
    check(res, { 'update: status 200': (r) => r.status === 200 });
}
