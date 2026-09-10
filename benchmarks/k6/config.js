// Настройки нагрузочных прогонов Mars. Всё переопределяется переменными окружения.

export const BASE_URL = (__ENV.MARS_URL || 'http://localhost:5003').replace(/\/+$/, '');
export const LOGIN = __ENV.MARS_LOGIN || 'testuser';
export const PASSWORD = __ENV.MARS_PASSWORD || 'Password123@';

// Сколько постов посеяно в стенде (post-0001..post-N)
export const POSTS_COUNT = parseInt(__ENV.MARS_POSTS || '1000', 10);

// Базлайнные сценарии: фиксированное окно нагрузки (constant-vus).
// 20 VU на чтение / 10 VU на запись; окно 30s — процентили из стационарного режима.
export const READ_VUS = parseInt(__ENV.MARS_READ_VUS || '20', 10);
export const WRITE_VUS = parseInt(__ENV.MARS_WRITE_VUS || '10', 10);
export const DURATION = __ENV.MARS_DURATION || '30s';

// Стресс-профили (ramping-vus): ищем потолок сервера — точку, где RPS перестаёт
// расти, а p95 уходит вверх. Запускаются отдельно (MARS_SCENARIO=stress_*).
export const STRESS_MAX_VUS = parseInt(__ENV.MARS_STRESS_VUS || '200', 10);

// Smoke-профиль (MARS_MODE=smoke): короткие shared-iterations для быстрой проверки
// «нет ли явных просадок» (~минута на всё). 20 VU осознанно: это до колена насыщения
// БД-сценариев, где замеры стабильнее (50 VU уже гоняют в зону роста latency).
export const SMOKE_ITERATIONS = parseInt(__ENV.MARS_SMOKE_ITERATIONS || '1000', 10);
