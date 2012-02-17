<?php

require_once APPPATH . 'config/feed.php';

class Content extends CI_Controller {

    public function visit($type, $contentID = false) {
        $this->load->helper('url');
        $this->load->model('content_model');

        switch ($type) {
            case FeedContentTypes::USER_CONTENT:
                $ugc = new Content_model();
                $item = $ugc->get_by_id($contentID);
                if ($item) {
                    header('Location: ' . $item->URL);
                }
                else {
                    die('No content found with given ID...');
                }
                break;
            case FeedContentTypes::TWEET:
                header('Location: ' . $_GET['url']);
                break;
            default:
                header('Location: ' . site_url(''));
                break;
        }
    }

}
