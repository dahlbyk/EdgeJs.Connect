var connect = require('connect');
var http = require('http');

var app = connect();
app.use(function (req, res, next) {
    console.log(`Node.js: ${req.url}`);
    next();
})

app.use('/test', function (req, res, next) {
    var buf = `Greetings from Node.js ${process.version}!\n`;

    res.setHeader('Content-Type', 'text/plain; charset=utf-8');
    res.setHeader('Content-Length', buf.length);
    res.end(buf);
})

function wrapReq(req) {
    return {
        url: req.Url,
    };
}

function wrapRes(req) {
    return {
        end: function () { return req.End(Array.prototype.slice.call(arguments)); },
        setHeader: function () { return req.SetHeader(Array.prototype.slice.call(arguments)); },
    };
}

module.exports = function owin(env, callback) {
    app(wrapReq(env.Request), wrapRes(env.Response), env.Next);

    return callback(null, env.Next());
};
