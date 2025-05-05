<?php
/**
 * Plugin Name: Auto Login as Guest
 * Description: Автоматически авторизует пользователя как гостя (user_id = 1) и устанавливает куки.
 */

// Подключаем WordPress core
require_once('wp-load.php');

// Проверяем, не авторизован ли уже пользователь
if (!is_user_logged_in()) {
    // Устанавливаем куки для гостя (user_id = 1)
    wp_clear_auth_cookie();
    wp_set_auth_cookie(1, true, is_ssl());

    // Перенаправляем пользователя на главную страницу или другую страницу
    wp_redirect(home_url());
    exit;
} else {
    // Если пользователь уже авторизован, перенаправляем его на главную страницу
    wp_redirect(home_url());
    exit;
}
