import { cpSync, mkdirSync } from 'node:fs';

mkdirSync('wwwroot/media', { recursive: true });
cpSync('node_modules/blockly/media', 'wwwroot/media', { recursive: true });
