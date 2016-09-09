module.exports = function(config) {
  config.set({
    frameworks: ['jasmine', 'chai'],
    reporters: ['progress'],
    logLevel: config.LOG_INFO,
    browsers: ['Chrome'],
    autoWatch: true,
    autoWatchBatchDelay: 300,

    files: [
      // Note: you must have already run 'webpack --config webpack.config.vendor.js' for this file to exist
      './wwwroot/dist/vendor.js',

      './ClientApp/tests/*.ts',
      './ClientApp/tests/**/*.ts'
    ],

    preprocessors: {
      './ClientApp/tests/*.ts': ['webpack'],
      './ClientApp/tests/**/*.ts': ['webpack']
    },

    webpack: require('./webpack.config.js'),
    webpackMiddleware: { stats: 'errors-only' }
  });
};
