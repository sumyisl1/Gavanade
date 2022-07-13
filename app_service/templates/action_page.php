<?php
$fname = $lname = $email = $agree = "";
$fnameErr = $lnameErr = $emailErr = $agreeErr = "";

/* 
$servername = "localhost";
$username = "username";
$password = "password";
$dbname = "myDB";

// Create connection
$conn = new mysqli($servername, $username, $password, $dbname);

// Check connection
if ($conn->connect_error) {
  die("Connection failed: " . $conn->connect_error);
}

$sql = "CREATE TABLE users (
  id INT(6) UNSIGNED AUTO_INCREMENT PRIMARY KEY,
  fname VARCHAR(30) NOT NULL,
  lname VARCHAR(30) NOT NULL,
  email VARCHAR(50),
  reg_date TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
  )";

if ($conn->query($sql) === TRUE) {
  echo "Table users created successfully";
} else {
  echo "Error creating table: " . $conn->error;
}
*/

if (empty($_POST["fname"])) {
  $fnameErr = "Name is required";
} else if (!preg_match("/^[a-zA-Z-' ]*$/",$fname)) {
  $fnameErr = "Only letters and white space allowed";
} else {
  $fname = test_input($_POST["fname"]);
}

if (empty($_POST["lname"])) {
  $lnameErr = "Name is required";
} else if (!preg_match("/^[a-zA-Z-' ]*$/",$lname)) {
  $lnameErr = "Only letters and white space allowed";
} else {
  $lname = test_input($_POST["lname"]);
}

if (empty($_POST["email"])) {
  $emailErr = "Email is required";
} else if (!filter_var($email, FILTER_VALIDATE_EMAIL)) {
  $emailErr = "Invalid email format";
} else {
  $email = test_input($_POST["email"]);
}

if (empty($_POST["agree"])) {
  $agreeErr = "Agreement to Terms and Conditions is required";
} else {
  $agree = test_input($_POST["agree"]);
}


function test_input($data) {
  $data = trim($data);
  $data = stripslashes($data);
  $data = htmlspecialchars($data);
  return $data;
}

/*$conn->close();*/
?>