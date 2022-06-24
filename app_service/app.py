from flask import Flask
from flask import render_template, request, flash, redirect, url_for

import requests

# from flask import g, session

app = Flask(__name__)
app.secret_key = b"U2hI]w1dKiD8NKGgxTUMaw5Deftwr3K7"
function_url = "https://gavanade-function-windows.azurewebsites.net/api"


@app.route("/")
def home():
    return render_template("index.html")


@app.route("/about")
def about():
    return render_template("about.html")


@app.route("/placeholder")
def placeholder():
    return render_template("placeholder.html")


@app.route("/legal")
def legal():
    return render_template("legal.html")


@app.route("/contact")
def contact():
    return render_template("contact.html")


@app.route("/privacypolicy")
def privacypolicy():
    return render_template("privacypolicy.html")


@app.route("/map")
def map():
    return render_template("map.html")


@app.route("/zip")
def zip():
    return render_template("zip.html")


@app.route("/search/zipcode", methods=("POST",))
def search_zipcode():
    zipcode = request.form["zipcode"]
    try:
        zipcode = int(zipcode)
        if 500 <= zipcode <= 99950:
            response = requests.get(
                f"{function_url}/gasprices?zipcode={zipcode}",
            )
            flash(response.text)
        else:
            zipcode = -2
    except BaseException:
        zipcode = -2

    return redirect(url_for("zip"))


@app.route("/search/coordinates", methods=("GET", "POST"))
def search_coordinates():
    lat = request.args.get("latitude", type=float)
    lon = request.args.get("longitude", type=float)
    lon = (lon + 180) % 360 - 180

    try:
        response = requests.get(
            f"{function_url}/gasprices?latitude={lat}&longitude={lon}",
        )
        flash(response.text)
    except BaseException:
        lat = -2
        lon = -2

    return redirect(url_for("map"))


if __name__ == "__main__":
    app.run()
