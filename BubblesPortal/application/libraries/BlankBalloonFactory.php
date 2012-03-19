<?php

require_once APPPATH . 'config/feed.php';

class BlankBalloonFactory {

    protected 
        $_idSequence;

    public function __construct() {
        // Initialise a base for our time based IDs
        // within the sliding window of balloon lifetime
        // http://stackoverflow.com/a/2480679/543200
        $interval = FeedContentTypes::BLANK_BALLOON_LIFETIME;
        $this->_idSequence = round(time() / $interval) * $interval;
    }

    protected function makeBlankBalloon() {
        return (object) array(
            'ContentID' => 'blank' . $this->_idSequence++,
            'BalloonColour' => '',
            'Title' => '',
            'Excerpt' => '',
            'SubmittedBy' => '',
            'URL' => site_url(''),
            'ImageURL' => '',
            'TimeCreated' => time(),
            'Type' => FeedContentTypes::BLANK_BALLOON
        );
    }

    public function getBalloons($number) {
        $number = abs($number);
        $blanks = array();

        while ($number > 0) {
            $blanks[] = $this->makeBlankBalloon();
            $number--;
        }

        return $blanks;
    }

}
