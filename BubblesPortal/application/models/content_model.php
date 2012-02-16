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

    private function setVoteLimits($minVotes = false, $maxVotes = false) {
        if ($minVotes !== false) {
            $this->db->where('Votes > ' . intval($minVotes));
        }
        if ($maxVotes !== false) {
            $this->db->where('Votes < ' . intval($maxVotes));
        }
    }

    public function get_recent($limit = 10, $sinceTime = false, $minVotes = false, $maxVotes = false) {
        if ($sinceTime !== false) {
            $this->db->where('TimeCreated > \'' . date('Y-m-d H:i:s', $sinceTime) . '\'');
        }
        
        $this->setVoteLimits($minVotes, $maxVotes);

        return $this->db->limit(intval($limit))
                        ->order_by('TimeCreated', 'desc')
                        ->get('usercontent')
                        ->result();
    }

    public function get_random_sample($limit = 10, $minVotes = false, $maxVotes = false) {
        $this->setVoteLimits($minVotes, $maxVotes);

        return $this->db->limit(intval($limit))
                        ->order_by('Votes', 'RANDOM')
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

    public function vote($id, $ratingChange) {
        $sign = ($ratingChange < 0) ? '-' : '+';
        $ratingChange = $sign . ' ' . abs($ratingChange);
        $this->db->set('Votes', 'Votes ' . $ratingChange, false);
        $this->db->where('ContentID', $id);
        $this->db->update('usercontent');
    }

}
