<?php

class Url_map extends CI_Model {

    const ID_BASE = 62;

    private $_baseUrl;

    public function set_base_url($url) {
        $this->_baseUrl = $url;
    }

    public function shorten($url) {
        $url = trim($url, '/');
        $sql = 'INSERT IGNORE INTO `url_map` (url) VALUES(?)';
        $result = $this->db->query($sql, array($url));
        $id = $this->db->insert_id();
        if ($id === 0) {
            $result = $this->db->get_where('url_map', array('url' => $url));
            $result = array_shift($result->result());
            $id = $result->id;
        }

        $gmpId = gmp_init($id, 10);
        return $this->_baseUrl . '/' . gmp_strval($gmpId, self::ID_BASE);
    }

    public function unshorten($id) {
        $gmpId = gmp_init($id, self::ID_BASE);
        $id = intval(gmp_strval($gmpId, 10));
        $result = $this->db->get_where('url_map', array('id' => $id));
        $result = array_shift($result->result());
        if ($result) {
            return $result->url;
        }
        else {
            return $this->_baseUrl;
        }
    }

}
