-- phpMyAdmin SQL Dump
-- version 4.4.14
-- http://www.phpmyadmin.net
--
-- Host: 127.0.0.1
-- Generation Time: Mar 14, 2016 at 07:00 PM
-- Server version: 5.6.26
-- PHP Version: 5.5.28

SET SQL_MODE = "NO_AUTO_VALUE_ON_ZERO";
SET time_zone = "+00:00";


/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!40101 SET NAMES utf8mb4 */;

--
-- Database: `osu!`
--

-- --------------------------------------------------------

--
-- Table structure for table `beatmaps_info`
--

CREATE TABLE IF NOT EXISTS `beatmaps_info` (
  `id` int(11) NOT NULL,
  `approved` tinyint(4) NOT NULL,
  `approved_date` date NOT NULL,
  `last_update` date NOT NULL,
  `set_id` int(11) NOT NULL,
  `artist` text NOT NULL,
  `creator` text NOT NULL,
  `source` text NOT NULL,
  `title` text NOT NULL,
  `version` text NOT NULL,
  `file_md5` varchar(32) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=latin1;

-- --------------------------------------------------------

--
-- Table structure for table `osu_info`
--

CREATE TABLE IF NOT EXISTS `osu_info` (
  `name` varchar(10) NOT NULL,
  `value` text NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=latin1;

-- --------------------------------------------------------

--
-- Table structure for table `users_info`
--

CREATE TABLE IF NOT EXISTS `users_info` (
  `id` int(11) NOT NULL,
  `username` varchar(15) NOT NULL,
  `password` text NOT NULL,
  `email` text NOT NULL,
  `country` varchar(2) NOT NULL,
  `reg_date` datetime NOT NULL,
  `last_login_date` datetime NOT NULL,
  `last_played_mode` tinyint(4) NOT NULL,
  `online_now` tinyint(1) NOT NULL,
  `tags` int(11) NOT NULL,
  `supporter` tinyint(1) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=latin1;

-- --------------------------------------------------------

--
-- Table structure for table `users_modes_info`
--

CREATE TABLE IF NOT EXISTS `users_modes_info` (
  `n` int(11) NOT NULL,
  `user_id` int(11) NOT NULL,
  `mode_id` tinyint(4) NOT NULL,
  `count300` int(10) unsigned NOT NULL,
  `count100` int(10) unsigned NOT NULL,
  `count50` int(10) unsigned NOT NULL,
  `countmiss` int(10) unsigned NOT NULL,
  `playcount` int(10) unsigned NOT NULL,
  `total_score` bigint(100) unsigned NOT NULL,
  `ranked_score` bigint(100) unsigned NOT NULL,
  `pp_rank` int(11) NOT NULL,
  `pp_raw` int(11) NOT NULL DEFAULT '1',
  `count_rank_ss` int(10) unsigned NOT NULL,
  `count_rank_s` int(10) unsigned NOT NULL,
  `count_rank_a` int(10) unsigned NOT NULL,
  `pp_country_rank` int(11) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=latin1;

-- --------------------------------------------------------

--
-- Table structure for table `users_replays`
--

CREATE TABLE IF NOT EXISTS `users_replays` (
  `user_id` int(11) NOT NULL,
  `beatmap_id` int(11) NOT NULL,
  `mode_id` int(11) NOT NULL,
  `replay` text NOT NULL,
  `date` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=latin1;

-- --------------------------------------------------------

--
-- Table structure for table `users_scores_info`
--

CREATE TABLE IF NOT EXISTS `users_scores_info` (
  `user_id` int(11) NOT NULL,
  `username` text NOT NULL,
  `beatmap_id` int(11) NOT NULL,
  `score_id` int(11) NOT NULL,
  `playMode` tinyint(4) NOT NULL,
  `count300` int(10) unsigned NOT NULL,
  `count100` int(10) unsigned NOT NULL,
  `count50` int(10) unsigned NOT NULL,
  `countmiss` int(10) unsigned NOT NULL,
  `total_score` int(11) NOT NULL,
  `maxcombo` int(10) unsigned NOT NULL,
  `countkatu` int(10) unsigned DEFAULT NULL,
  `countgeki` int(10) unsigned DEFAULT NULL,
  `perfect` varchar(5) NOT NULL,
  `enabled_mods` int(11) NOT NULL,
  `date` varchar(11) NOT NULL,
  `rank` varchar(2) NOT NULL,
  `pp` float NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=latin1;

--
-- Indexes for dumped tables
--

--
-- Indexes for table `beatmaps_info`
--
ALTER TABLE `beatmaps_info`
  ADD PRIMARY KEY (`id`);

--
-- Indexes for table `users_info`
--
ALTER TABLE `users_info`
  ADD PRIMARY KEY (`id`),
  ADD UNIQUE KEY `id` (`id`);

--
-- Indexes for table `users_modes_info`
--
ALTER TABLE `users_modes_info`
  ADD PRIMARY KEY (`n`);

--
-- Indexes for table `users_scores_info`
--
ALTER TABLE `users_scores_info`
  ADD PRIMARY KEY (`score_id`),
  ADD UNIQUE KEY `score_id` (`score_id`);

--
-- AUTO_INCREMENT for dumped tables
--

--
-- AUTO_INCREMENT for table `beatmaps_info`
--
ALTER TABLE `beatmaps_info`
  MODIFY `id` int(11) NOT NULL AUTO_INCREMENT;
--
-- AUTO_INCREMENT for table `users_info`
--
ALTER TABLE `users_info`
  MODIFY `id` int(11) NOT NULL AUTO_INCREMENT;
--
-- AUTO_INCREMENT for table `users_modes_info`
--
ALTER TABLE `users_modes_info`
  MODIFY `n` int(11) NOT NULL AUTO_INCREMENT;
--
-- AUTO_INCREMENT for table `users_scores_info`
--
ALTER TABLE `users_scores_info`
  MODIFY `score_id` int(11) NOT NULL AUTO_INCREMENT;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
