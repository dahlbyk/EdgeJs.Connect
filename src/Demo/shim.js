module.exports = function owin(env, callback) {
    if (env.Path === '/test') {
        return callback();
    }

    return callback(null, env.Next());
};
