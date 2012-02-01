<!DOCTYPE html>
<html>
	<head>
		<title>MACS news server</title>
		<link rel="stylesheet" href="<?php echo base_url(); ?>css/login.css" type="text/css" />
		<meta http-equiv="Content-Type" content="text/html;charset=utf-8" >
	</head>
	
	<body>
		<div id="header">
			<div id="heading"><h1>Access the MACS news server</h1></div>
		</div>
		<div id="form_container">
			<div id="form">
				<?php echo validation_errors('<p class="errors">'); ?>
				<?php echo (isset($errors) ? '<p class="errors">'.$errors.'</p>' : ''); ?>
				<?php echo form_open('welcome/validate_user'); ?>
				<form action="<?php echo $_SERVER['PHP_SELF']; ?>" method="post">
				<div class="form-item"><label for="username">Username:</label><input name="username" type="text" value="Username" onclick="this.value=''" /></div>
				<div class="form-item"><label for="password">Password:</label><input name="password" type="password" value="Password" onclick="this.value=''" /></div>
				<div class="form-item"><input name="submit" type="submit" value="Enter" /></div>
				<?php echo form_close(); ?>
			</div>
		</div>
	</body>
</html>