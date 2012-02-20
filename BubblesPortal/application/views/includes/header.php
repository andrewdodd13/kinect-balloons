<?php
$is_logged_in = $this->session->userdata('is_logged_in');
$username = $this->session->userdata('username');
$group = $this->session->userdata('group');
if($username == "") {
	$username = "Sign In";
}
else {
	$username = "Logged in as: $username of $group";
}
?>
<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Strict//EN" 
	"http://www.w3.org/TR/xhtml1/DTD/xhtml1-strict.dtd">
<html xmlns="http://www.w3.org/1999/xhtml" xml:lang="en" lang="en">

<head>
	<title>HW News Cloud</title>
	<meta http-equiv="content-type" content="text/html;charset=utf-8" />
	<link rel="stylesheet" href="<?php echo base_url(); ?>included/main.css" />
	<?php if(isset($js)): ?>
	<?php foreach($js as $jsItem): ?>
	<script type="text/javascript" src="<?php echo $jsItem['src']; ?>"></script>
	<?php endforeach; ?>
	<?php endif; ?>
</head>

<body>
	<div id="header">
		<img src="<?php echo base_url(); ?>included/logo.png" alt="HW logo" />
		<h1>HW News Portal</h1>
		<ul>
			<li>
				<a href="<?php echo site_url('content_manager/index'); ?>">View Articles</a>
				<a href="<?php echo site_url('welcome/index'); ?>"><?php echo $username; ?></a>
				<?php if(isset($is_logged_in)): ?>
					<?php if($is_logged_in === true): ?>
						<a href="<?php echo site_url('submit_content/index'); ?>">Submit an article</a>
						<a href="<?php echo site_url('welcome/logout'); ?>">Log out</a>
					<?php endif; ?>
				<?php endif; ?>
			</li>
		</ul>
	</div>