import string
import cherrypy
import json
import numpy as np
import tensorflow as tf

class Network():
	def __init__(self):
		self.actions = 4
		self.num_inputs = self.actions + 4
		self.batchsize = 64
		self.discount_rate = 0.75
		self.lr = 0.001
		self.optimizer = tf.optimizers.Adam(self.lr)
		
		self.experience = {'s': [], 'a': [], 'r': [], 'ns': [], 'done': []}
		self.counter = 0

		self.policyNetwork = self.ModelTemplate()
		# self.policyNetwork.compile(optimizer=self.optimizer,
		# 		loss=['mae'],
		# 		metrics=['mae'])

		self.targetNetwork = self.ModelTemplate()
		self.copyNN()
		print(self.policyNetwork.summary())

	def ModelTemplate(self):
			model = tf.keras.Sequential([
							tf.keras.layers.Flatten(),
							tf.keras.layers.Dense(64, activation='relu'),
							tf.keras.layers.Dense(64, activation='relu'),
							tf.keras.layers.Dense(64, activation='relu'),
							tf.keras.layers.Dense(self.actions)
						])
			model.build(input_shape=(1,self.num_inputs))			
			return model

	def fit(self, exp):
		self.counter += 1
		for key, value in exp.items():
			self.experience[key].append(value)
			
		if(self.counter==self.batchsize):
			states = tf.convert_to_tensor(self.experience['s']) #The current state
			actions = np.asarray(self.experience['a']) #The action performed (chosen randomly or by net)
			rewards = np.asarray(self.experience['r']) #The reward got
			next_states = tf.convert_to_tensor(self.experience['ns']) #The next state
			dones = np.asarray(self.experience['done']) #State of the game (ended or not)
			# print(type(next_states), np.shape(next_states), next_states)
			#value_next = np.max(self.predictTN(next_states),axis=1) #Max next values according to the Target Network
			value_next = np.max(self.targetNetwork(next_states),axis=1) #Max next values according to the Target Network
			
			# print('oj')
			# print(type(rewards), rewards.shape,rewards)
			# print(type(value_next), value_next.shape, value_next)
			actual_values = np.where(dones, rewards, rewards+self.discount_rate*value_next)

			with tf.GradientTape() as tape:
				# print(type(states), states.shape, states)
				tape.watch(states)
				a = self.policyNetwork(states) * tf.one_hot(actions, self.actions)
				selected_action_values = tf.math.reduce_sum(a , axis=1)
				# print('partial', type(a), a.shape, a)
				# print('final', type(selected_action_values), selected_action_values.shape, selected_action_values)
				loss = tf.math.reduce_mean(tf.square(actual_values - selected_action_values))
				tape.watch(loss)


			variables = self.policyNetwork.trainable_variables			
			gradients = tape.gradient(loss, variables)
			# print('loss', type(loss), loss)
			# print(type(variables), np.array(variables).shape)
			# print('wv', tape.watched_variables())
			# print(type(gradients), np.array(gradients).shape)
			# print(gradients)
        
			self.optimizer.apply_gradients(zip(gradients, variables))

			self.counter=0
			self.experience = {'s': [], 'a': [], 'r': [], 'ns': [], 'done': []}

			return loss		

	def predictPN(self, input):
		return self.policyNetwork(np.atleast_2d(input))

	def predictTN(self, input):
		return self.targetNetwork(np.atleast_2d(input))
	
	def copyNN(self):
		self.targetNetwork.set_weights(self.policyNetwork.get_weights())


class CalculatorWebService(object):
	#Required to be accessable online
	exposed=True

	def __init__(self):
		self.net = Network()

	def FIT(self):
		msg = cherrypy.request.body.readline().decode("utf-8")
		exp = self.decodeMsg(msg)
		self.net.fit(exp)

	def PREDICT_PN(self):
		msg = cherrypy.request.body.readline().decode("utf-8")
		input = np.array(msg.split('.')).astype(int)
		output = self.net.predictPN(input)
		out = '.'.join([str(o) for o in output.numpy()[0]])
		#out = out.strip()[1:-1]
		#print('pn', out, np.shape(out))
		return out

	def PREDICT_TN(self):
		output = self.net.predictTN(values[0])
		out = '.'.join([str(o) for o in output])
		#print('tn', out, np.shape(out))
		return out

	def COPY_NN(self):
		self.net.copyNN()

	def decodeMsg(self, msg):
		values = msg.split(',')

		currentstate = tf.Variable([np.array(values[0].split('.'), dtype=np.int8)])
		action = int(values[1])
		nextstate = tf.Variable([np.array(values[2].split('.'), dtype=np.int8)])
		reward = float(values[3])
		endstate = bool(values[4])

		exp = {'s': currentstate, 'a': action, 'r': reward, 'ns': nextstate, 'done': endstate}
		return exp

	# def GET(self,*path,**query):
	# 	output = 'Get'
	# 	return output

	# def POST(self,*path,**query):
	# 	msg = cherrypy.request.body.read()
	# 	print(msg)
	# 	output = 'Post'
	# 	return output

	# def PUT(self,*path,**query):
	# 	msg = cherrypy.request.body.read().decode("utf-8") 
	# 	msg = msg.split('.')
	# 	result = {'action':msg[4], 'value':[0.35,0]}
	# 	output = json.dumps(result)
	# 	return output

	# def DELETE(self,*path,**query):
	# 	output = 'Get'
	# 	return output

#'request.dispatch': cherrypy.dispatch.MethodDispatcher() => switch from default URL to HTTP compliant approch
conf = { '/': {	'request.dispatch': cherrypy.dispatch.MethodDispatcher(),
								'tools.sessions.on':True} 
					}
cherrypy.tree.mount(CalculatorWebService(), '/', conf)

cherrypy.config.update({'servet.socket_host':'0.0.0.0'})
cherrypy.config.update({'servet.socket_port':'8080'})

cherrypy.engine.start()
cherrypy.engine.block()
