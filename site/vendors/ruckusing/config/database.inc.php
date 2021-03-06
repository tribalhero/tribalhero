<?php

if (!defined('DS')) {
	define('DS', DIRECTORY_SEPARATOR);
}
	
if (!defined('APP_DIR')) {
	define('APP_DIR', dirname(dirname(RUCKUSING_BASE)) . DS . 'app');
}

require_once APP_DIR . DS . 'Config' . DS . 'database.php';

$dbConfig = new DATABASE_CONFIG();

$driverParts = explode('/', $dbConfig->default['datasource'], 2);

//----------------------------
// DATABASE CONFIGURATION
//----------------------------
$ruckusing_db_config = array(
	
    'development' => array(
        'type'      => strtolower($driverParts[1]),
        'host'      => $dbConfig->default['host'],
        'port'      => !empty($dbConfig->default['port']) ? $dbConfig->default['port'] : 3306,
        'database'  => $dbConfig->default['database'],
        'user'      => $dbConfig->default['login'],
        'password'  => $dbConfig->default['password']
    ),

	'test' => array(
        'type'      => strtolower($driverParts[1]),
        'host'      => $dbConfig->test['host'],
        'port'      => !empty($dbConfig->test['port']) ? $dbConfig->test['port'] : 3306,
        'database'  => $dbConfig->test['database'],
        'user'      => $dbConfig->test['login'],
        'password'  => $dbConfig->test['password']
	)
	
);