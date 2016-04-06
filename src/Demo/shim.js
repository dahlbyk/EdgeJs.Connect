var connect = require('connect');
var http = require('http');

var app = connect();

module.exports = function owin(env, callback) {
    app(env.Request, env.Response, env.Next);

    return callback(null, env.Next());
};
