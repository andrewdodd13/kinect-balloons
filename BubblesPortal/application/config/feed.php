<?php

// Twitter blue :-)
define('TWITTER_BALLOON_COLOR', '9AE4E8');

define('TWITTER_CONSUMER_KEY', '6ceCGZYBKP6LMB2eOIeAiA');
define('TWITTER_CONSUMER_SECRET', 'kKiUFtFQN3VhrnZETJHGbxlBQAfd7PVuNTgAPXnx0');
define('TWITTER_USER_TOKEN', '35330150-lDrWdpn8bqVwK7Xlf8g1mcolbsFwN70g3ja2Us');
define('TWITTER_USER_SECRET', 'VbOYDAVT5haX1oBJJ5FXBRYt1aoK3bvQOS43mnOKo');

final class FeedContentTypes {
    const USER_CONTENT = 1;
    const BLANK_BALLOON = 2;
    const TWEET = 3;

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
            self::USER_CONTENT  => 3,
            self::BLANK_BALLOON => 1,
            self::TWEET         => 2,
        );
    }
}
