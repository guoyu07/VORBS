﻿(function () {

    angular.module('vorbs.services')
        .factory('LocationsService', LocationsService);

    LocationsService.$inject = ['$resource'];

    function LocationsService($resource) {
        return $resource('/api/locations', {},
            {
                query: {
                    method: 'GET',
                    isArray: true
                },
                getByID: {
                    method: 'GET',
                    params: {
                        id: '@id'
                    },
                    url: '/api/locations/:id'
                },
                getByStatus: {
                    method: 'GET',
                    isArray: true,
                    params: {
                        status: '@status',
                        extraInfo: '@extraInfo'
                    },
                    url: '/api/locations/:status/:extraInfo'
                },
                create: {
                    method: 'POST'
                },
                update: {
                    method: 'PUT',
                    params: {
                        id: '@id'
                    },
                    url: '/api/locations/:id'
                },
                updateStatus: {
                    method: 'PATCH',
                    params: {
                        id: '@id',
                        status: '@status'
                    },
                    url: '/api/locations/:id/:status'
                }
            });
    }

})();