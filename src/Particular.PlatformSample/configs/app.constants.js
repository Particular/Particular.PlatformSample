;(function (window, angular, undefined) {  'use strict';

    angular.module('sc')
        .constant('version', '1.15.0')
        .constant('showPendingRetry', false)
        .constant('scConfig', {
            default_route: '/dashboard',
            service_control_url: 'http://localhost:{ServiceControlPort}/api',
            monitoring_urls: ['http://localhost:{MonitoringPort}/']
        });

}(window, window.angular));
