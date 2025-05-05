#!/​bin/sh
first_arg_wp_url="$1"
echo "INSTALL_START"

#export WP_URL=$first_arg_wp_url
echo WP_URL=$WP_URL

#curl 'https://raw.githubusercontent.com/wp-cli/builds/gh-pages/phar/wp-cli.phar' -o "wp-cli.phar"
#chmod +x "wp-cli.phar"
#mv 'wp-cli.phar' '/usr/local/bin/wp'
wp core install --allow-root --url=$WP_URL --title=MyWordPress --admin_user=admin --admin_email=admin@example.com --admin_password=admin --skip-email --locale=ru_RU
wp --allow-root rewrite structure '/%postname%/'
wp --allow-root language core install ru_RU
wp --allow-root site switch-language ru_RU

echo "INSTALL_END"
