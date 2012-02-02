<?php

define('TWITTER_USER', 'cgwyllie');

class Api extends CI_Controller {

    public function getFeedJson($limit, $sinceTime) {
        $this->load->model('content_model');
        $ugc = new Content_model();
        $recent = $ugc->get_recent($limit, $sinceTime);

        foreach ($recent as $r) {
            $r->Image = base64_encode($r->Image);
            $r->TimeCreated = strtotime($r->TimeCreated);
            $r->Type = 'UGC';
        }

        $recent = array_merge($recent,
                              $this->getTwitterUpdates(TWITTER_USER));
        
        header('Content-Type: application/json');
        die(json_encode($recent));
    }

    protected function getTwitterUpdates($twitterUser) {
        $this->load->driver('cache');

        $updates = array();
        $twitterXML = $this->cache->file->get('twitterCache.'.$twitterUser);
        if ($twitterXML === false) {
		    $twitterXML = file_get_contents('http://twitter.com/statuses/user_timeline.xml?screen_name=' . $twitterUser);
		    $this->cache->file->save('twitterCache'.$twitterUser, $twitterXML, (60*10));
        }
		
        $xml = new SimpleXMLElement($twitterXML);
		foreach ($xml->xpath('//status[in_reply_to_screen_name=""]') as $update) {
            $updates[] = (object) array(
                'SubmittedBy' => TWITTER_USER,
                'URL' => 'http://twitter.com/status/' . $update->id,
                'Title' => (string) $update->text,
                'TimeCreated' => strtotime($update->created_at),
                'Excerpt' => '',
                'Image' => '',
                'ContentID' => ''
            );
        }

        return $updates;
    }
}
