<?php

class Users_model extends CI_Model {
	
	public function validate($username, $password) {
		//check is a user
		$userInfo = $this->get_user_info($username);
		if($userInfo == null || (!isset($password))) {
			return false;
		}
		
		$encryptedSystemPassword = $userInfo['passwd'];
		if(crypt($password, $encryptedSystemPassword) == $encryptedSystemPassword ) {
			return true;
		}
		
		return false;
	}
	
	public function get_user_info($username) {
		//check is a user
		$userInfo = posix_getpwnam($username);
		return $userInfo;
	}
	
	public function get_user_group($gid) {
		$usergroup = posix_getgrgid($gid);
		return $usergroup;
	}
}