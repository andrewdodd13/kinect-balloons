<?php

// Twitter blue :-)
define('TWITTER_BALLOON_COLOR', '9AE4E8');

define('TWITTER_CONSUMER_KEY', 'AlK5OoSA7VnCPaX3Tq4w');
define('TWITTER_CONSUMER_SECRET', 'Jpul1N0XNmD2kPTwqW2x6zknZBIEufO0JGSHkkfHN4');
define('TWITTER_USER_TOKEN', '220056690-R3z1idJtvJdO7BWORtKCu9IZhAB2sEwgYEViT1pA');
define('TWITTER_USER_SECRET', 'tQEAHHTJyCJ5hkzXY6OAMRKipV7hJ0bGrAXVSQz2dfo');

final class FeedContentTypes {
    
    const TOP_USER_CONTENT = 0;
    const USER_CONTENT = 1;
    const BLANK_BALLOON = 2;
    const TWEET = 3;

    // Balloons with > this many votes are TOP_USER_CONTENT
    // Of course if they are voted down, they will be regular
    // balloons again.
    const VOTES_TO_STARDOM = 4;
    // Balloons with <= this many votes won't show up at all
    const VOTES_TO_DIE = -3;

    // 15 minutes in seconds
    const BLANK_BALLOON_LIFETIME = 60;

    // If there isn't enough of some content
    // types, what should make up the difference?
    const PAD_WITH = self::TWEET;

    // This should be the sum of the ratio
    const TOTAL_PARTS = 6;

    /**
     * This returns the ratio of each content type in
     * the feed. When modified, the feed automatically
     * adjusts the number of each item based on the number
     * of items requested and this ratio.
     * 
     * To completely remove a content type, just comment it
     * out and update the TOTAL_PARTS constant above.
     * 
     * @return array
     */
    public static function ratio() {
        return array(
            self::TOP_USER_CONTENT  => 1,
            self::USER_CONTENT      => 2,
            self::BLANK_BALLOON     => 1,
            self::TWEET             => 2,
        );
    }
}
