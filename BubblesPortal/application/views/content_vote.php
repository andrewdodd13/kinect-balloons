<!DOCTYPE html> 
<html> 
    <head> 
    <title>Rate this content!</title> 
    <meta name="viewport" content="width=device-width, initial-scale=1"> 
    <link rel="stylesheet" href="http://code.jquery.com/mobile/1.0.1/jquery.mobile-1.0.1.min.css" />
    <script src="http://code.jquery.com/jquery-1.6.4.min.js"></script>
    <script src="http://code.jquery.com/mobile/1.0.1/jquery.mobile-1.0.1.min.js"></script>
    <style type="text/css">
    .ui-btn {padding: 12px;}
    </style>
</head> 
<body> 

<div data-role="page">

    <div data-role="header">
        <h1>"<?php echo $item->Title; ?>"</h1>
    </div><!-- /header -->

    <div data-role="content">   
        <p>Is this content any good?</p>
        <div data-role="controlgroup">
            <a href="<?php echo $voteUpUrl; ?>" data-role="button" data-icon="arrow-u">Yes - vote up!</a>
            <a href="<?php echo $voteDownUrl; ?>" data-role="button" data-icon="arrow-d">No - vote down!</a>
        </div>
        <a href="<?php echo $item->URL; ?>" data-role="button" data-icon="arrow-r">Meh - don't vote...</a>
        <p>Popular content will display on the screens for longer, allowing more people to see it!</p>
        <p>Content that's voted down repeatedly will disappear quickly so it doesn't waste your time.</p>
    </div><!-- /content -->

</div><!-- /page -->

</body>
</html>
