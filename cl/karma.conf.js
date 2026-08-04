// Karma configuration — Story C-A (docs/stories/brownfield-client-a-test-harness.md).
// Runner choice and its argument: docs/refactoring/client-refactor-design.md §4.
module.exports = function (config) {
  config.set({
    basePath: '',
    frameworks: ['jasmine', '@angular-devkit/build-angular'],
    plugins: [
      require('karma-jasmine'),
      require('karma-chrome-launcher'),
      require('karma-jasmine-html-reporter'),
      require('karma-coverage'),
      require('@angular-devkit/build-angular/plugins/karma'),
    ],
    client: {
      jasmine: {
        // The restore cascade is order-dependent by nature; a random spec order would make a
        // leaked localStorage draft or a leaked HTTP expectation look like a flaky race.
        random: false,
      },
      clearContext: false, // leave the Jasmine HTML report visible in the browser
    },
    jasmineHtmlReporter: { suppressAll: true },
    coverageReporter: {
      dir: require('path').join(__dirname, './coverage/k-sail-calculator'),
      subdir: '.',
      reporters: [{ type: 'html' }, { type: 'text-summary' }],
    },
    reporters: ['progress', 'kjhtml'],
    browsers: ['Chrome'],
    customLaunchers: {
      ChromeHeadlessCI: {
        base: 'ChromeHeadless',
        flags: ['--no-sandbox', '--disable-gpu'],
      },
    },
    restartOnFileChange: true,
  });
};
