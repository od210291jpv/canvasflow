// client/src/components/SkeletonLoader.js
import React from 'react';

const SkeletonLoader = () => {
    return (
        <div style={{ display: 'flex', gap: '20px', padding: '20px 0' }}>
            <div style={{ flex: 1, minWidth: '300px' }}>
                <div style={{ height: '200px', background: '#e0e0e0', borderRadius: '8px', marginBottom: '15px' }}></div>
                <div style={{ height: '24px', background: '#e0e0e0', width: '70%', borderRadius: '4px', marginBottom: '8px' }}></div>
                <div style={{ height: '16px', background: '#e0e0e0', width: '50%', borderRadius: '4px', marginBottom: '15px' }}></div>
                <div style={{ height: '40px', background: '#e0e0e0', borderRadius: '8px' }}></div>
            </div>
            <div style={{ flex: 1, minWidth: '300px' }}>
                <div style={{ height: '200px', background: '#e0e0e0', borderRadius: '8px', marginBottom: '15px' }}></div>
                <div style={{ height: '24px', background: '#e0e0e0', width: '70%', borderRadius: '4px', marginBottom: '8px' }}></div>
                <div style={{ height: '16px', background: '#e0e0e0', width: '50%', borderRadius: '4px', marginBottom: '15px' }}></div>
                <div style={{ height: '40px', background: '#e0e0e0', borderRadius: '8px' }}></div>
            </div>
            <div style={{ flex: 1, minWidth: '300px' }}>
                <div style={{ height: '200px', background: '#e0e0e0', borderRadius: '8px', marginBottom: '15px' }}></div>
                <div style={{ height: '24px', background: '#e0e0e0', width: '70%', borderRadius: '4px', marginBottom: '8px' }}></div>
                <div style={{ height: '16px', background: '#e0e0e0', width: '50%', borderRadius: '4px', marginBottom: '15px' }}></div>
                <div style={{ height: '40px', background: '#e0e0e0', borderRadius: '8px' }}></div>
            </div>
        </div>
    );
};

export default SkeletonLoader;