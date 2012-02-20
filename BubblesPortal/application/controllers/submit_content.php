<?php if ( ! defined('BASEPATH')) exit('No direct script access allowed');

class Submit_content extends CI_Controller {
	
	public function __construct() {
		parent::__construct();
		$this->is_logged_in();
	}
	
	public function index() {
		$this->show_submit_article_page();
	}
	
	public function show_submit_article_page($errors = '') {
		$data['main_content'] = 'submit_content_view';
		$data['headerData']['js']['jsItem']['src'] = base_url()."included/jscolor/jscolor.js";
		$data['bodyData'] = $errors;
		
		$this->load->view('includes/template', $data);
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
		$colour = $this->input->post('colour');
		if(!isset($colour)) {
			$colour = 'FFFFFF';
		}
		$this->form_validation->set_rules('name', 'Article name', 'trim|required|min_length[4]');
		$this->form_validation->set_rules('url', 'URL location', 'trim|required|min_length[4]');
		$this->form_validation->set_rules('colour', 'Balloon colour', 'trim|exact_length[6]');
		
		if($this->form_validation->run() == FALSE) {
			$this->show_submit_article_page();
		}
		else {
			// configuration for uploading files
			$uploadConfig = array();
			$uploadConfig['upload_path'] = '/u1/cs4/amm22/public_html/bubblesportal/uploads/';
			$uploadConfig['allowed_types'] = 'gif|jpg|png';
			$uploadConfig['max_size'] = '200';
			$uploadConfig['max_width'] = '1024';
			$uploadConfig['max_height'] = '1024';
			
			// load library
			$this->load->library('upload', $uploadConfig);
			
			// process file from input tag with name 'image'
			if(!$this->upload->do_upload('image')) {
				// file has failed to upload
				$errors = array('errors' => $this->upload->display_errors('<p class="errors">', '</p>'));
				$this->show_submit_article_page($errors);
			}
			else {
				// file has uploaded
				// enter information into database
				// get file url
				$fileInfo = $this->upload->data();
				$imageURL = base_url()."uploads/".$fileInfo['file_name'];
				
				// load the model
				$this->load->model('content_model');
				// use insert content method
				$this->content_model->insert_content($name, $url, $imageURL, $this->session->userdata('username'), $colour);
				$this->upload_successful();
			}
		}
	}
	
	public function upload_successful() {
		$errors = array(
			'errors' => 'Your article has been successfully submitted. Thank You'
		);
		
		$this->show_submit_article_page($errors);
	}
	
}