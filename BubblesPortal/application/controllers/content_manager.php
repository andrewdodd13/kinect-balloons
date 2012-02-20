<?php if ( ! defined('BASEPATH')) exit('No direct script access allowed');

class Content_manager extends CI_Controller {
	
	public function index() {
		$this->show_content();
	}
	
	
	public function show_content() {
		$this->load->model('content_model');
		
		$recent = $this->content_model->get_recent();
		
		$data = array(
			'articles' => $recent
		);
		
		$data['main_content'] = 'content_viewer';
		$this->load->view('includes/template', $data);
	}
	
	public function remove_content($contentID) {
		$this->load->model('content_model');
		$this->content_model->remove_content_by_id($contentID);
		$this->show_content();
	}
}
