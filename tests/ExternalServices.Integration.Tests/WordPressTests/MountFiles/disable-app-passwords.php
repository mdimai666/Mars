<?php
/**
 * Стенд Mars: WordPress 5.6+ сам обрабатывает Authorization: Basic как Application Passwords
 * и отвечает 401 на логин/пароль пользователя, поэтому встроенный механизм отключаем —
 * доступы стенда проверяют плагин WP-API/Basic-Auth.
 */
add_filter( 'wp_is_application_passwords_available', '__return_false' );
add_filter( 'wp_is_application_passwords_available_for_user', '__return_false' );
