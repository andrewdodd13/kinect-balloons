<?php

class Users_model extends CI_Model {
	
	public function validate($username, $password) {
		//check is a user
		$userInfo = posix_getpwnam($username);
		if($userInfo == null || (!isset($password))) {
			return false;
		}
		
		$encryptedSystemPassword = $userInfo['passwd'];
		if(crypt($password, $encryptedSystemPassword) == $encryptedSystemPassword ) {
			return true;
		}
		
		return false;
	}
}