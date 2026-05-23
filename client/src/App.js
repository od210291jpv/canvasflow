// client/src/App.js
import React from 'react';
import { BrowserRouter as Router, Routes, Route, Navigate } from 'react-router-dom';
import Layout from './components/Layout';
import Login from './pages/Login';
import Feed from './pages/Feed';

// Simple Protected Route component
const ProtectedRoute = ({ children }) => {
    const isAuthenticated = localStorage.getItem('authToken');
    return isAuthenticated ? children : <Navigate to="/login" />;
};

function App() {
    return (
        <Router>
            <Layout>
                <Routes>
                    {/* Public Route */}
                    <Route path="/login" element={<Login />} />
                    
                    {/* Protected Routes */}
                    <Route 
                        path="/" 
                        element={
                            <ProtectedRoute>
                               <Feed />
                            </ProtectedRoute>
                        } 
                    />
                    <Route 
                        path="/feed" 
                        element={
                            <ProtectedRoute>
                               <Feed />
                            </ProtectedRoute>
                        } 
                    />
                    {/* Fallback */}
                    <Route path="*" element={<Navigate to="/" />} />
                </Routes>
            </Layout>
        </Router>
    );
}

export default App;