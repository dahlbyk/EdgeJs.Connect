module.exports = function owin(env, callback) {
    if (env.Path === '/test') {
        env.WriteText("Hello from Node.js");
        return callback();
    }

    return callback(null, env.Next());
};
