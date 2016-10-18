var isDevBuild = process.argv.indexOf('--env.prod') < 0;
var path = require('path');
var webpack = require('webpack');
var AureliaWebpackPlugin = require('aurelia-webpack-plugin');
const srcDir = path.resolve('./ClientApp');
const rootDir = path.resolve();
const outDir = path.resolve('./wwwroot/dist');
const baseUrl = '/';
var project = require('./package.json');

const aureliaBootstrap = [
    'aurelia-bootstrapper-webpack',
    'aurelia-polyfills',
    'aurelia-pal-browser',
    'regenerator-runtime',
];
const aureliaModules = Object.keys(project.dependencies).filter(dep => dep.startsWith('aurelia-'));

// Configuration for client-side bundle suitable for running in browsers
var clientBundleConfig = {
     resolve: { extensions: [ '.js', '.ts' ] },
     devtool: isDevBuild ? 'inline-source-map' : null,
     entry: {
        'app': [], // <-- this array will be filled by the aurelia-webpack-plugin
        'aurelia-bootstrap': aureliaBootstrap,
        'aurelia-modules': aureliaModules.filter(pkg => aureliaBootstrap.indexOf(pkg) === -1)
    },
    output: {
        path: outDir,
        publicPath: '/dist',
        filename: '[name]-bundle.js'
    },
    module: {
        loaders: [
            { 
                test: /\.ts$/, 
                exclude: /node_modules/, 
                loader: 'ts-loader',
            }, {
                test: /\.html$/,
                exclude: /index\.html$/,
                loader: 'html-loader'
            }, {
                test: /\.css$/,
                loaders: ['style-loader', 'css-loader']
            },
            { test: /\.(png|woff|woff2|eot|ttf|svg)$/, loader: 'url-loader?limit=100000' }
        ]
    },
    plugins: [
        new webpack.ProvidePlugin({
            regeneratorRuntime: 'regenerator-runtime', // to support await/async syntax
            Promise: 'bluebird', // because Edge browser has slow native Promise object
            $: 'jquery', // because 'bootstrap' by Twitter depends on this
            jQuery: 'jquery', // just an alias
            'window.jQuery': 'jquery' // this doesn't expose jQuery property for window, but exposes it to every module
        }),
        new AureliaWebpackPlugin({
            root: rootDir,
            src: srcDir,
            baseUrl: baseUrl
        }),
        new webpack.optimize.CommonsChunkPlugin({
            name: ['aurelia-modules', 'aurelia-bootstrap']
        }),
    ].concat(isDevBuild ? [] : [
        // Plugins that apply in production builds only
        new webpack.optimize.OccurenceOrderPlugin(),
        new webpack.optimize.UglifyJsPlugin()
    ])
};
 
module.exports = [clientBundleConfig];
