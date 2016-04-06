var connect = require('connect');
var http = require('http');

var app = connect();
app.use(function (req, res, next) {
    console.log(`Node.js: ${req.url}`);
    next();
})

function wrapReq(req) {
    return {
        url: req.Url,
    };
}

function wrapRes(req) {
    return {
    };
}

module.exports = function owin(env, callback) {
    app(wrapReq(env.Request), wrapRes(env.Response), env.Next);

    return callback(null, env.Next());
};
