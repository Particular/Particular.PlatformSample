;(function(window, angular, undefined) {
    'use strict';

    window.config = {
        default_route: '{DefaultRoute}',
        service_control_url: 'http://localhost:{ServiceControlPort}/api/',
        monitoring_urls: ['http://localhost:{MonitoringPort}/']
    };

    angular.module('sc')
        .constant('version', '{Version}')
        .constant('showPendingRetry', false)
        .constant('scConfig', window.config);

}(window, window.angular));
