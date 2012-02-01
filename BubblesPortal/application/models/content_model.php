<?php

class Content_model extends CI_Model {
	
	public function insert_content($name, $url, $image, $user) {
		$data = array(
			'Title' => $name,
			'SubmittedBy' => $user,
			'URL' => $url,
			'Image' => $image
		);
		
		$this->db->insert('usercontent', $data);
		
		return $this->db->insert_id();
	}

    public function get_recent($limit = 10, $sinceTime = false) {
        if ($sinceTime !== false) {
            $this->db->where('TimeCreated > \'' . date('Y-m-d H:i:s', $sinceTime) . '\'');
        } 
        return $this->db->limit(intval($limit))
                        ->order_by('TimeCreated', 'desc')
                        ->get('usercontent')
                        ->result();
    }
	
}
