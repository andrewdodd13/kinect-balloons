<?php

class Submit_content extends CI_Controller {
	
	public function __construct() {
		parent::__construct();
		$this->is_logged_in();
	}
	
	public function index() {
		$this->load->view('submit_content_view', array('errors' => ''));
	}
	
	public function is_logged_in() {
		$is_logged_in = $this->session->userdata('is_logged_in');
		
		if(!isset($is_logged_in) || $is_logged_in != true) {
			redirect('welcome/index');
		}
	}
	
	public function upload_complete() {
		$name = $this->input->post('name');
		$url = $this->input->post('url');
		
		// configuration for uploading files
		$uploadConfig = array();
		$uploadConfig['upload_path'] = '/u1/cs4/amm22/public_html/researchproject/uploads/';
		$uploadConfig['allowed_types'] = 'gif|jpg|png';
		$uploadConfig['max_size'] = '200';
		$uploadConfig['max_width'] = '640';
		$uploadConfig['max_height'] = '640';
		
		// load library
		$this->load->library('upload', $uploadConfig);
		
		// process file from input tag with name 'image'
		if(!$this->upload->do_upload('image')) {
			// file has failed to upload
			$errors = array('errors' => $this->upload->display_errors('<p class="errors">', '</p>'));
			$this->load->view('submit_content_view', $errors);
		}
		else {
			// file has uploaded
			// enter information into database
			// get file data
			$fileInfo = $this->upload->data();
			$filename = $fileInfo['full_path'];
			$fh = fopen($filename, 'r');
			$imageData = fread($fh, filesize($filename));
			fclose($fh);
			
			// load the model
			$this->load->model('content_model');
			// use insert content method
			$this->content_model->insert_content($name, $url, $imageData, $this->session->userdata('username'));
			$this->upload_successful();
		}
	}
	
	public function upload_successful() {
		$this->load->view('upload_success');
	}
	
}