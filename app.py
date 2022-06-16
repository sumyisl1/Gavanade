from flask import Flask
from flask import render_template, redirect, url_for, request, g, session, flash
import requests

app = Flask(__name__)
app.secret_key = b'fjsdjkhf1u34y23cas/fsgg.'

@app.route("/")
def home():
    return "hello"

if __name__ == '__main__':
   app.run()