<?php

class Url extends CI_Controller {

    public function __construct() {
        parent::__construct();
        $this->load->model('url_map');
    }

    public function go($shortId) {
        $url = $this->url_map->unshorten($shortId);
        if ($url !== false) {
            header('Location: ' . $url);
        }
        else {
            die('Url not found!');
        }
    }

}
