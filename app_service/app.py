from flask import Flask
from flask import render_template, request, flash, redirect, url_for

# from flask import g, session
# import requests

app = Flask(__name__)
app.secret_key = b"U2hI]w1dKiD8NKGgxTUMaw5Deftwr3K7"


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

@app.route("/gasprices")
def gasprices():
    return render_template("gasprices.html") 

@app.route("/search", methods=("POST",))
def search():
    zipcode = request.form["zipcode"]
    try:
        zipcode = int(zipcode)
        if 501 <= zipcode <= 99950:
            # handle zip codes
            flash("you have entered a valid zipcode")
        else:
            zipcode = -1
    except BaseException:
        zipcode = -1

    flash(f"{zipcode}")
    return redirect(url_for("home"))


if __name__ == "__main__":
    app.run()
