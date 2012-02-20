<script type="text/javascript">
function removehash() {
	var colourElementValue = document.getElementById("colour").value;
	
	if(colourElementValue.indexOf("#") != -1) {
		document.getElementById("colour").value = colourElementValue.substring(1);
	}
}
</script>

<div id="form_container" style='height:400px;'>
	<div id="form">
		<?php echo form_open_multipart('submit_content/upload_complete'); ?>
		<p>Please submit an article page, giving us the URL of the page, a title and an image to represent it.</p>
		<?php echo validation_errors('<p class="errors">'); ?>
		<?php echo (isset($errors) ? '<p class="errors">'.$errors.'</p>' : ''); ?>
		<div class="form-item"><label for="name">Article name:</label><input name="name" type="text" value="Article Name" onclick="this.value=''" /></div>
		<div class="form-item"><label for="url">Article URL:</label><input name="url" type="text" value="Article URL" onclick="this.value=''" /></div>
		<div class="form-item"><label for="image">Article image:</label><input name="image" type="file" value="Article Image" /></div>
		<div class="form-item"><label for="colour">Balloon colour:</label><input class="color" id="colour" name="colour" type="text" value="#FFFFFF" /></div>
		<div class="form-item"><input name="submit" type="submit" value="Submit" onclick="removehash()" /></div>
		</form>
	</div>
</div>