var isDevBuild = process.argv.indexOf('--env.prod') < 0;
var path = require('path');
var webpack = require('webpack');
var ExtractTextPlugin = require('extract-text-webpack-plugin');

module.exports = {
    devtool: isDevBuild ? 'inline-source-map' : null,
    entry: { 'main': './ClientApp/boot.tsx' },
    resolve: { extensions: [ '', '.js', '.jsx', '.ts', '.tsx' ] },
    output: {
        path: path.join(__dirname, './wwwroot/dist'),
        filename: '[name].js',
        publicPath: '/dist/'
    },
    module: {
        loaders: [
            { test: /\.ts(x?)$/, include: /ClientApp/, loader: 'babel-loader' },
            { test: /\.tsx?$/, include: /ClientApp/, loader: 'ts', query: { silent: true } },
            { test: /\.css$/, loader: isDevBuild ? 'style!css' : ExtractTextPlugin.extract(['css']) },
            { test: /\.(png|jpg|jpeg|gif|svg)$/, loader: 'url', query: { limit: 25000 } }
        ]
    },
    plugins: [
        new webpack.DllReferencePlugin({
            context: __dirname,
            manifest: require('./wwwroot/dist/vendor-manifest.json')
        })
    ].concat(isDevBuild ? [
        // Plugins that apply in development builds only
        new webpack.SourceMapDevToolPlugin({ moduleFilenameTemplate: '../../[resourcePath]' }) // Compiled output is at './wwwroot/dist/', but sources are relative to './'
    ] : [
        // Plugins that apply in production builds only
        new webpack.optimize.OccurenceOrderPlugin(),
        new webpack.optimize.UglifyJsPlugin({ compress: { warnings: false } }),
        new ExtractTextPlugin('site.css')
    ])
};
