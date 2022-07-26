<?php
$fname = $lname = $email = $agree = "";
$fnameErr = $lnameErr = $emailErr = $agreeErr = "";

if (empty($_POST["fname"])) {
  $fnameErr = "Name is required";
} else if (!preg_match("/^[a-zA-Z-' ]*$/",$fname)) {
  $fnameErr = "Only letters and white space allowed";
}

if (empty($_POST["lname"])) {
  $lnameErr = "Name is required";
} else if (!preg_match("/^[a-zA-Z-' ]*$/",$lname)) {
  $lnameErr = "Only letters and white space allowed";
} 

if (empty($_POST["email"])) {
  $emailErr = "Email is required";
} else if (!filter_var($email, FILTER_VALIDATE_EMAIL)) {
  $emailErr = "Invalid email format";
} 
if (empty($_POST["agree"])) {
  $agreeErr = "Agreement to Terms and Conditions is required";
}

?>