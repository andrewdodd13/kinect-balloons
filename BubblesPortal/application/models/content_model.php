<?php

class Content_model extends CI_Model {
	
	public function insert_content($name, $url, $image, $user, $colour) {
		$data = array(
			'Title' => $name,
			'SubmittedBy' => $user,
			'URL' => $url,
			'ImageURL' => $image,
			'BalloonColour' => $colour
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

    public function get_by_id($id) {
        $result = $this->db->where('ContentID', $id)->get('usercontent')->result();
        if (isset($result[0])) {
            return $result[0];
        }
        else {
            return false;
        }
    }
	
	public function remove_content_by_id($contentID) {
		$this->db->delete('usercontent', array('ContentID' => $contentID));
	}
}
