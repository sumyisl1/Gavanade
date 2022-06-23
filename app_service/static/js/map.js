var map = L.map('map').fitWorld();
L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors',
id: 'mapbox.streets',
accessToken: 'pk.eyJ1Ijoia2F5bGllbmF5bG9yIiwiYSI6ImNsNHB4ZnJsYjBsem4zZHBhcmUwejB5ejIifQ.mNLOIprK0Ui7NrT36JRkcg'}).addTo(map);

var marker;

map.on('click', 
    function(e) {
        if (marker) { // check
            map.removeLayer(marker); // remove
        }
        var popup = L.popup()
            .setLatLng(e.latlng)
            .setContent('<p>You are here!</p>');
        var coord = e.latlng; // no need for toString()
        var lat = coord.lat;
        var lng = coord.lng;
        marker = L.marker(e.latlng, {draggable:'true'}).addTo(map)
        .bindPopup('Create an entry <a href="/search/coordinates?latitude='+lat+'&longitude='+lng+'"> here</a>').openPopup();
});