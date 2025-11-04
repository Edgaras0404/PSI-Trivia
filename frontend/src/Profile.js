import React, { useState, useEffect } from 'react';
import { User, Trophy, Target, Calendar, TrendingUp, Award, ArrowLeft, Settings } from 'lucide-react';
import './Profile.css';
import LiquidChrome from './LiquidChrome';

const Profile = ({ username, onBack }) => {
    const [userData, setUserData] = useState(null);
    const [loading, setLoading] = useState(true);

    useEffect(() => {
        const fetchUserData = async () => {
            try {
                const response = await fetch(`https://localhost:5001/api/leaderboard/rank/${username}`);
                if (response.ok) {
                    const data = await response.json();
                    setUserData(data);
                } else {
                    console.error('Failed to fetch user data');
                }
            } catch (error) {
                console.error('Error fetching user data:', error);
            } finally {
                setLoading(false);
            }
        };

        fetchUserData();
    }, [username]);

    if (loading) {
        return (
            <>
                <div className="liquid-chrome-background">
                    <LiquidChrome
                        baseColor={[0.4, 0.5, 0.9]}
                        speed={0.5}
                        amplitude={0.6}
                        frequencyX={3}
                        frequencyY={3}
                        interactive={true}
                    />
                </div>
                <div className="container">
                    <div className="loading">Loading profile...</div>
                </div>
            </>
        );
    }

    if (!userData) {
        return (
            <>
                <div className="liquid-chrome-background">
                    <LiquidChrome
                        baseColor={[0.4, 0.5, 0.9]}
                        speed={0.5}
                        amplitude={0.6}
                        frequencyX={3}
                        frequencyY={3}
                        interactive={true}
                    />
                </div>
                <div className="container">
                    <div className="card">
                        <h2>Unable to load profile</h2>
                        <button onClick={onBack} className="button button-primary">
                            <ArrowLeft className="icon" />
                            Back to Menu
                        </button>
                    </div>
                </div>
            </>
        );
    }

    const stats = [
        { label: "Games Played", value: userData.gamesPlayed, icon: Target, color: "#3b82f6" },
        { label: "Total Points", value: userData.totalPoints?.toLocaleString() || '0', icon: Trophy, color: "#f59e0b" },
        { label: "Current ELO", value: userData.elo, icon: TrendingUp, color: "#10b981" },
        { label: "Global Rank", value: `#${userData.rank}`, icon: Award, color: "#667eea" }
    ];

    return (
        <>
            <div className="liquid-chrome-background">
                <LiquidChrome
                    baseColor={[0.4, 0.5, 0.9]}
                    speed={0.5}
                    amplitude={0.6}
                    frequencyX={3}
                    frequencyY={3}
                    interactive={true}
                />
            </div>
            <div className="container" style={{ paddingTop: '20px' }}>
                <div className="card" style={{ maxWidth: '800px' }}>
                    {/* Header */}
                    <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: '30px' }}>
                        <button
                            onClick={onBack}
                            className="button button-secondary"
                            style={{ display: 'flex', alignItems: 'center', gap: '8px' }}
                        >
                            <ArrowLeft className="icon" />
                            Back
                        </button>
                        <h2 style={{ margin: 0, color: '#1a202c', fontSize: '28px' }}>Player Profile</h2>
                        <button className="button" style={{ background: '#f3f4f6', color: '#4b5563' }}>
                            <Settings className="icon" />
                        </button>
                    </div>

                    {/* Profile Card */}
                    <div style={{
                        background: 'linear-gradient(135deg, #667eea 0%, #764ba2 100%)',
                        padding: '30px',
                        borderRadius: '12px',
                        marginBottom: '24px',
                        color: 'white'
                    }}>
                        <div style={{ display: 'flex', alignItems: 'center', gap: '20px' }}>
                            {/* Avatar */}
                            <div style={{
                                width: '80px',
                                height: '80px',
                                background: 'rgba(255, 255, 255, 0.2)',
                                borderRadius: '50%',
                                display: 'flex',
                                alignItems: 'center',
                                justifyContent: 'center',
                                border: '3px solid rgba(255, 255, 255, 0.3)'
                            }}>
                                <User style={{ width: '40px', height: '40px' }} />
                            </div>

                            {/* User Info */}
                            <div style={{ flex: 1 }}>
                                <h1 style={{ margin: '0 0 8px 0', fontSize: '32px' }}>{userData.username}</h1>
                                <div style={{ display: 'flex', alignItems: 'center', gap: '8px', opacity: 0.9 }}>
                                    <Calendar style={{ width: '16px', height: '16px' }} />
                                    <span>Total Players: {userData.totalPlayers}</span>
                                </div>
                            </div>
                        </div>
                    </div>

                    {/* Stats Grid */}
                    <div style={{
                        display: 'grid',
                        gridTemplateColumns: 'repeat(auto-fit, minmax(160px, 1fr))',
                        gap: '16px',
                        marginBottom: '24px'
                    }}>
                        {stats.map((stat, index) => {
                            const Icon = stat.icon;
                            return (
                                <div
                                    key={index}
                                    style={{
                                        background: '#f7fafc',
                                        padding: '20px',
                                        borderRadius: '12px',
                                        border: '2px solid #e2e8f0',
                                        transition: 'all 0.2s'
                                    }}
                                    onMouseEnter={(e) => {
                                        e.currentTarget.style.borderColor = stat.color;
                                        e.currentTarget.style.transform = 'translateY(-2px)';
                                    }}
                                    onMouseLeave={(e) => {
                                        e.currentTarget.style.borderColor = '#e2e8f0';
                                        e.currentTarget.style.transform = 'translateY(0)';
                                    }}
                                >
                                    <Icon style={{ width: '32px', height: '32px', color: stat.color, marginBottom: '12px' }} />
                                    <div style={{ fontSize: '28px', fontWeight: 'bold', color: '#1a202c', marginBottom: '4px' }}>
                                        {stat.value}
                                    </div>
                                    <div style={{ fontSize: '14px', color: '#718096' }}>
                                        {stat.label}
                                    </div>
                                </div>
                            );
                        })}
                    </div>

                    {/* Performance Details */}
                    <div style={{
                        background: '#f7fafc',
                        padding: '24px',
                        borderRadius: '12px',
                        border: '2px solid #e2e8f0'
                    }}>
                        <h3 style={{ margin: '0 0 20px 0', color: '#1a202c', fontSize: '20px' }}>
                            Performance Details
                        </h3>
                        <div style={{
                            display: 'grid',
                            gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))',
                            gap: '12px'
                        }}>
                            <div style={{
                                display: 'flex',
                                justifyContent: 'space-between',
                                padding: '12px',
                                background: 'white',
                                borderRadius: '8px'
                            }}>
                                <span style={{ color: '#718096' }}>Average Score</span>
                                <span style={{ fontWeight: 'bold', color: '#1a202c' }}>
                                    {userData.gamesPlayed > 0
                                        ? Math.round(userData.totalPoints / userData.gamesPlayed)
                                        : 0
                                    }
                                </span>
                            </div>
                            <div style={{
                                display: 'flex',
                                justifyContent: 'space-between',
                                padding: '12px',
                                background: 'white',
                                borderRadius: '8px'
                            }}>
                                <span style={{ color: '#718096' }}>Rank Position</span>
                                <span style={{ fontWeight: 'bold', color: '#667eea' }}>
                                    Top {userData.totalPlayers > 0
                                        ? Math.round((userData.rank / userData.totalPlayers) * 100)
                                        : 0
                                    }%
                                </span>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </>
    );
};


export default Profile;