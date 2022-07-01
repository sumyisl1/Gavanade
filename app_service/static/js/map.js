customBounds = new L.LatLngBounds(new L.LatLng(24.2, -125), new L.LatLng(49.5, -67));
var map = L.map('map', {
    center: customBounds.getCenter(),
    zoom: 5,
    maxBounds: customBounds,
    maxBoundsViscosity: 1
})
L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
    minZoom: 5,
    attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors',
    id: 'mapbox.streets',
    accessToken: 'pk.eyJ1Ijoia2F5bGllbmF5bG9yIiwiYSI6ImNsNHB4ZnJsYjBsem4zZHBhcmUwejB5ejIifQ.mNLOIprK0Ui7NrT36JRkcg'
}).addTo(map);

var latlngs = L.rectangle(customBounds).getLatLngs();
L.polyline(latlngs[0].concat(latlngs[0][0])).addTo(map);

var marker;
map.on('click',
    function (e) {
        if (marker) { // check
            map.removeLayer(marker); // remove
        }
        var popup = L.popup()
            .setLatLng(e.latlng)
            .setContent('<p>You are here!</p>');
        var coord = e.latlng; 
        var lat = coord.lat;
        var lng = coord.lng;
        window.location = '/search/coordinates?latitude=' + lat + '&longitude=' + lng;
        marker = L.marker(e.latlng, { draggable: 'true' }).addTo(map);
    });