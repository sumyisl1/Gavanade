from flask import Flask
from flask import render_template

# from flask import redirect, url_for, request, g, session, flash
# import requests

app = Flask(__name__)
app.secret_key = b"U2hI]w1dKiD8NKGgxTUMaw5Deftwr3K7"


@app.route("/")
def home():
    return render_template("index.html")


@app.route("/about")
def about():
    return render_template("about.html")


@app.route("/search/<zipcode>", methods=("POST",))
def search(zipcode=0):
    return f"searched {zipcode}"


if __name__ == "__main__":
    app.run()
