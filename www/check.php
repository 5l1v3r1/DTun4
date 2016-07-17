<?php
if($_GET['version'] < 40) {//10-12 //13-15 //16 //20-24 //30-36
	echo 'MESS Download new installer from: http://dtun4.disahome.me';
	exit;
}
if($_GET['version'] < 40){//36
	echo 'BAD';
	exit;
}
echo 'OK';
exit;
?>