module.exports = function owin(env, callback) {
    callback(null, {
        path: env.Path,
        handled: env.Path === '/test'
    });
    return;
};
