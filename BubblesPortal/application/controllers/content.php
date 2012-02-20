<?php

require_once APPPATH . 'config/feed.php';

class Content extends CI_Controller {

    public function __construct() {
        parent::__construct();
        $this->load->helper('url');
        $this->load->model('content_model');
    }

    public function visit($type, $contentID = false) {

        switch ($type) {
            case FeedContentTypes::USER_CONTENT:
                $ugc = new Content_model();
                $item = $ugc->get_by_id($contentID);
                if ($item) {
                    $this->load->view('content_vote', array(
                        'item' => $item,
                        'voteUpUrl' => site_url('content/up/' . $item->ContentID),
                        'voteDownUrl' => site_url('content/down/'.$item->ContentID)
                    ));
                }
                else {
                    die('No content found with given ID...');
                }
                break;
            default:
                header('Location: ' . site_url(''));
                break;
        }
    }

    private function vote($contentID, $votes) {
        $item = $this->content_model->get_by_id($contentID);
        if ($item) {
            $this->content_model->vote($contentID, $votes);
            header('Location: ' . $item->URL);
        }
    }

    public function up($contentID) {
        $this->vote($contentID, 1);
    }

    public function down($contentID) {
        $this->vote($contentID, -1);
    }

}
