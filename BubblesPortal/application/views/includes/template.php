<?php

if(isset($headerData)) {
	$this->load->view('includes/header', $headerData);
}
else {
	$this->load->view('includes/header');
}

if(isset($bodyData)) {
	$this->load->view($main_content, $bodyData);
}
else {
	$this->load->view($main_content);
}

$this->load->view('includes/footer');
?>