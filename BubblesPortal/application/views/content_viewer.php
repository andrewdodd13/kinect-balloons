<?php
$is_logged_in = $this->session->userdata('is_logged_in');
$username = $this->session->userdata('username');
$group = $this->session->userdata('group');

/** 
 * Script expects an array of objects containing details of each 
 * article in the database
 * 
 * $articles ( article1, article2 ... )
 * 
 * $article1 (
 *   ContentID
 *   Title
 *   SubmittedBy
 *   URL
 *   BalloonColour
 *   ImageURL
 * )
 */
?>
<div id="SignInDiv">
	<input id="btnSignUp" name="btnSignIn" type="button" value="Sign in to submit an article" onclick="window.location='<?php echo site_url('welcome/index'); ?>'" />
</div>
<div id="content">
<table id="contenttable" cellpadding="0" cellspacing="0" border="0" class="table">
	<thead>
		<tr>
			<th>Title</th>
			<th>Submitted By</th>
			<th>Article URL</th>
			<th>Balloon Colour</th>
			<th>Image</th>
			<?php if(isset($is_logged_in)): ?>
			<?php if($group == 'staff'): ?>
			<th>Remove Article</th>
			<?php endif; ?>
			<?php endif; ?>
		</tr>
	</thead>
	<tbody>
		<?php foreach( $articles as $article): ?>
		<tr>
			<td><?php echo $article->Title; ?></td>
			<td><?php echo $article->SubmittedBy; ?></td>
			<td><a href="<?php echo $article->URL; ?>"><?php echo $article->URL; ?></td>
			<td style="background-color:#<?php echo $article->BalloonColour; ?>;"><?php echo $article->BalloonColour; ?></td>
			<td><img src="<?php echo $article->ImageURL; ?>" alt="Image" width="100" height="100" /></td>
			<?php if(isset($is_logged_in)): ?>
				<?php if($group == "staff"): ?>
					<td><a href="<?php echo site_url('content_manager/remove_content/'.$article->ContentID); ?>">Remove</td>
				<?php endif; ?>
			<?php endif; ?>
		</tr>
		<?php endforeach; ?>
	</tbody>
</table>
</div>