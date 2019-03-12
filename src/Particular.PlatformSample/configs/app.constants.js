;(function(window, angular, undefined) {
    'use strict';

    window.config = {
        default_route: '{DefaultRoute}',
        service_control_url: 'http://localhost:{ServiceControlPort}/api/',
        monitoring_urls: ['http://localhost:{MonitoringPort}/']
    };

    angular.module('sc')
        .constant('version', '1.15.0')
        .constant('showPendingRetry', false)
        .constant('scConfig', window.config);

}(window, window.angular));
