;(function (window, angular, undefined) {  'use strict';

    angular.module('sc')
        .constant('version', '1.14.4')
        .constant('showPendingRetry', false)
        .constant('scConfig', {
            default_route: '/dashboard',
            service_control_url: 'http://localhost:33333/api/',
            monitoring_urls: ['']
        });

}(window, window.angular));
