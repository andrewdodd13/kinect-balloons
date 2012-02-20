<?php

require_once APPPATH . 'config/feed.php';
require_once APPPATH . 'libraries/tmhOAuth/tmhOAuth.php';
require_once APPPATH . 'libraries/BlankBalloonFactory.php';

class Api extends CI_Controller {
	
    public function __construct() {
        parent::__construct();
        $this->load->helper('url');
        $this->load->model('url_map');
        $this->url_map->set_base_url(site_url('url/go'));
        
        $this->load->model('content_model');
    }

    protected function setupUserContentBalloon($balloon, $type) {
        $balloon->type = $type;
        $balloon->URL = $this->url_map->shorten(
            site_url('content/visit/' . $type . '/' . $balloon->ContentID)
        );
        $balloon->TimeCreated = strtotime($balloon->TimeCreated);
    }

    protected function getUserContent($limit) {

        $recent = $this->content_model->get_recent($limit,
            false, // since any time
            FeedContentTypes::VOTES_TO_DIE,
            FeedContentTypes::VOTES_TO_STARDOM
        );

        foreach ($recent as $r) {
            $this->setupUserContentBalloon($r, FeedContentTypes::USER_CONTENT);
        }

        return $recent;
    }

    protected function getTopUserContent($limit) {
        $top = $this->content_model->get_random_sample($limit,
                FeedContentTypes::VOTES_TO_STARDOM
        );

        foreach ($top as $r) {
            $this->setupUserContentBalloon($r, FeedContentTypes::TOP_USER_CONTENT);
        }

        return $top;
    }

    protected function getBlankBalloons($number) {
        $factory = new BlankBalloonFactory();
        return $factory->getBalloons($number);
    }
    
    protected function makeBalloonFromTweet($tweet) {
        $permalink = sprintf('http://twitter.com/%s/status/%s',
                                $tweet->user->screen_name,
                                $tweet->id);
        $tweetURL = $this->url_map->shorten($permalink);

        return (object) array(
            'ContentID' => 't' . $tweet->id,
            'Title' => 'Tweet from @' . $tweet->user->screen_name,
            'SubmittedBy' => $tweet->user->screen_name,
            'TimeCreated' => strtotime($tweet->created_at),
            'Excerpt' => $tweet->text,
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
                case FeedContentTypes::TOP_USER_CONTENT:
                    $items = $this->getTopUserContent($number);
                    break;
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
