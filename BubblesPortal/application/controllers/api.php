<?php

require_once(APPPATH . 'config/feed.php');
require_once(APPPATH . 'libraries/tmhOAuth/tmhOAuth.php');

class Api extends CI_Controller {

    public function __construct() {
        parent::__construct();
        // Isn't this already loaded in autoload???? Alex
        $this->load->helper('url');
    }

    protected function getUserContent($limit) {
        $this->load->model('content_model');

        $ugc = new Content_model();
        $recent = $ugc->get_recent($limit);

        foreach ($recent as $r) {
            $r->Type = FeedContentTypes::USER_CONTENT;
            $r->URL = site_url('content/visit/' . $r->Type . '/' . $r->ContentID);
            $r->TimeCreated = strtotime($r->TimeCreated);
        }

        return $recent;
    }

    protected function makeBlankBalloon() {
        return (object) array(
            'ContentID' => 'blank',
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

    protected function getBlankBalloons($number) {
        $number = abs($number);
        $blanks = array();

        while ($number > 0) {
            $blanks[] = $this->makeBlankBalloon();
            $number--;
        }

        return $blanks;
    }

    protected function makeBalloonFromTweet($tweet) {
        $permalink = urlencode(sprintf('http://twitter.com/%s/status/%s',
                                $tweet->user->screen_name,
                                $tweet->id));
        $tweetURL = site_url('content/visit/' . FeedContentTypes::TWEET . '/?url=' . $permalink);

        return (object) array(
            'ContentID' => 't' . $tweet->id,
            'Title' => $tweet->text,
            'SubmittedBy' => $tweet->user->screen_name,
            'TimeCreated' => strtotime($tweet->created_at),
            'Excerpt' => '',
            'URL' => $tweetURL,
            'ImageURL' => $tweet->user->profile_image_url,
            'BalloonColour' => TWITTER_BALLOON_COLOR,
            'Type' => FeedContentTypes::TWEET
        );
    }

    protected function getTweets($number) {
        $items = array();

        $twitter = new tmhOAuth(array(
            'consumer_key' => TWITTER_CONSUMER_KEY,
            'consumer_secret' => TWITTER_CONSUMER_SECRET,
            'user_token' => TWITTER_USER_TOKEN,
            'user_secret' => TWITTER_USER_SECRET
        ));

        $responseCode = $twitter->request('GET', $twitter->url('1/statuses/home_timeline'), array(
            'count' => $number,
            'contributor_details' => true
        ));
        
        if (200 == $responseCode) {
            $tweets = json_decode($twitter->response['response']);
            foreach ($tweets as $tweet) {
                $items[] = $this->makeBalloonFromTweet($tweet);
            }
        }

        return $items;
    }

    protected function getFeedItems($type, $number) {
        $items = array();
        if ($number > 0) {
            switch ($type) {
                case FeedContentTypes::USER_CONTENT:
                    $items = $this->getUserContent($number);
                    break;
                case FeedContentTypes::BLANK_BALLOON:
                    $items = $this->getBlankBalloons($number);
                    break;
                case FeedContentTypes::TWEET:
                    $items = $this->getTweets($number);
                    break;
            }
        }

        return $items;
    }

    public function getFeed($totalItems = 10) {
        $totalItems = abs($totalItems);
        $totalParts = 0;
        
        $feed = array();

        foreach (FeedContentTypes::ratio() as $type => $number) {
            if (FeedContentTypes::PAD_WITH == $type) {
                continue;
            }
            $count = round(($number / FeedContentTypes::TOTAL_PARTS) * $totalItems,
                            0,
                            PHP_ROUND_HALF_ODD);
            $feed = array_merge($feed, $this->getFeedItems($type, $count));
        }
        
        $padCount = $totalItems - count($feed);
        $feed = array_merge($feed, $this->getFeedItems(FeedContentTypes::PAD_WITH, $padCount));

        header('Content-Type: application/json');
        echo json_encode($feed);
    }

}
