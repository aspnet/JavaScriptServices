module.exports = {
    devtool: 'inline-source-map',
    module: {
        loaders: [
            { test: /\.css/, loader: 'style-loader!css-loader' }
        ]
    }
};
